<template>
  <div class='pl-4 pr-3'>
    <!-- <v-btn icon> -->
    <template v-for='layer in layers'>
      <v-layout>
        <v-flex>
          {{layer.name}}
        </v-flex>
        <v-flex>
          {{layer.properties.color.hex}}
        </v-flex>
        <v-flex>
          {{layer.objectCount}}
        </v-flex>
      </v-layout>
    </template>
    </v-layout>
  </div>
</template>
<script>
export default {
  name: 'SenderLayers',
  props: {
    layers: Array,
    objects: Array
  },
  components: {},
  computed: {},
  data( ) {
    return {}
  },
  methods: {},
  mounted( ) {}
}
</script>
<style lang='scss'>
</style>