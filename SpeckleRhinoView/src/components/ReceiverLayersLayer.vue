<template>
  <v-layout @mouseover='mouseOver' @mouseleave='mouseOut' align-center>
    <v-flex xs2>
      <v-btn icon small xs flat @click.native='visible=!visible' color='grey'>
      <v-icon dark>{{ visible ? "visibility" : "visibility_off" }}</v-icon>
      </v-btn>
    </v-flex>
    <v-flex xs10>
      <div class='subheading'>{{ layer.name }}</div>
      <div class="caption grey--text">Object count: {{layer.objectCount}}</div>
    </v-flex>
  </v-layout>
</template>

<script>

  export default {
    name: '',
    props: {
      layer: Object
    },
    components: {},
    computed: {},
    data() {
      return {
        visible: true
      }
    },
    methods: {
      mouseOver() {
        console.log('in')
      },
      mouseOut() {
        console.log('out')
      }
    },
    mounted() {
    }
  }
</script>

<style lang='scss'>
    
</style>