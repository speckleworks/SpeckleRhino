<template>
  <v-layout @mouseover='mouseOver' @mouseleave='mouseOut' align-center>
    <v-flex class='xs8 text-xs-left layername pl-4'>
      <span class=''>{{ layer.name }}</span>
      <span class="caption grey--text"> Object count: {{layer.objectCount }} </span>
    </v-flex>
    <v-flex class='xs1 text-xs-center'>
      <v-btn icon small flat @click.native='visible=!visible' @dblclick='toggleAll' color='grey' xxclass='ma-0'>
        <v-icon dark class='xs-actions'>{{ visible ? "visibility" : "visibility_off" }}</v-icon>
      </v-btn>
    </v-flex>
    <v-flex class='xs1 text-xs-center'>
      <v-btn icon small xs flat @click.native='bake' color='grey' xxclass='ma-0'>
        <v-icon dark class='xs-actions'>play_for_work</v-icon>
      </v-btn>
    </v-flex>
    <v-flex class=' text-xs-center'>
      <v-icon dark :style='{ color: layerColor.hex }' class='make-me-small'>fiber_manual_record</v-icon>
    </v-flex>
  </v-layout>
</template>
<script>
import { EventBus } from '../event-bus'

export default {
  name: '',
  props: {
    layer: Object,
    clientId: String
  },
  components: {},
  computed: {
    layerColor( ) {
      if ( this.layer.properties && this.layer.properties.color )
        return this.layer.properties.color
      return { hex: '#AEECFD', alpha: 1 }
    }
  },
  watch: {
    visible( value ) {
      Interop.setLayerVisibility( this.clientId, this.layer.guid, value )
    }
  },
  data( ) {
    return {
      visible: true
    }
  },
  methods: {
    toggleAll( ) {
      this.visible = !this.visible
      EventBus.$emit( 'layer-set-all-vis', { state: this.visible, clientId: this.clientId } )
    },
    mouseOver( ) {
      Interop.setLayerHover( this.clientId, this.layer.guid, true )
    },
    mouseOut( ) {
      Interop.setLayerHover( this.clientId, this.layer.guid, false )
    },
    bake( ) {
      Interop.bakeLayer( this.clientId, this.layer.guid )
    }
  },
  mounted( ) {
    EventBus.$on( 'layer-set-all-vis', data => {
      if ( this.clientId != data.clientId ) return
      this.visible = data.state
    } )
  }
}
</script>
<style lang='scss'>
.make-me-small {
  font-size: 14px !important;
}

.layername {
  max-width: 50%;
  text-overflow: ellipsis;
  /* Required for text-overflow to do anything */
  white-space: nowrap;
  overflow: hidden;
}
</style>